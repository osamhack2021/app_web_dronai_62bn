import React from 'react';
// material-ui
import { Grid, Link, Pagination } from '@material-ui/core';
import MuiTypography from '@material-ui/core/Typography';

// project imports
import SubCard from './../../ui-component/cards/SubCard';
import MainCard from './../../ui-component/cards/MainCard';
import SecondaryAction from './../../ui-component/cards/CardSecondaryAction';
import { gridSpacing } from './../../store/constant';

import imgA from './../../assets/images/logo.png';


//==============================|| TYPOGRAPHY ||==============================//

const TestList = () => {
    return (
        <MainCard title="Test List" secondary={<SecondaryAction link="https://next.material-ui.com/system/typography/" />}>
            <Grid container spacing={gridSpacing}>
                <Grid item xs={12} sm={6}>
                    <SubCard title="Extra">
                        <Grid container direction="column" spacing={1}>


                            <img src={imgA} width="100" height="100" align="middle"/>

                            
                            
                            <Grid item>
                                <MuiTypography variant="button" display="block" gutterBottom>
                                    Drone ID
                                </MuiTypography>
                            </Grid>
                            <Grid item>
                                <MuiTypography variant="caption" display="block" gutterBottom>
                                    caption text
                                </MuiTypography>
                            </Grid>
                            <Grid item>
                                <MuiTypography variant="overline" display="block" gutterBottom>
                                    Details
                                </MuiTypography>
                            </Grid>
                            <Grid item>
                                <MuiTypography
                                    variant="body2"
                                    color="primary"
                                    component={Link}
                                    href="https://linearjun.com"
                                    target="_blank"
                                    display="block"
                                    gutterBottom
                                    underline="hover"
                                >
                                    https://linearjun.com
                                </MuiTypography>
                            </Grid>
                        </Grid>
                    </SubCard>
                </Grid>
            </Grid>

            <Pagination count={10} color="primary" />

        </MainCard>
    );
};

export default TestList;
